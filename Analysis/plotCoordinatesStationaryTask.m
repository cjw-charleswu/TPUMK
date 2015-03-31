function [] = plotCoordinatesStationaryTask(coordinates_average_study_table, coordinates_average_scenario_table)

joints_util;
plot_colors;

main_title = 'Coordinates Averages for the Stationary Task';
dir = 'Plots/Stationary_Task/';
main_filename = strcat(dir,'Coordinates_Stationary_Task');

coordinate_dx_idx = 4;
coordinate_dy_idx = 6;
coordinate_dz_idx = 8;
coordinate_dd_idx = 10;

kinect_config_types = {
  'Parallel', '45 Degrees-apart','90 Degrees-apart','Average'
};

scen_table = coordinates_average_study_table(coordinates_average_study_table.Scenario_Id==1,:);
rows = size(scen_table,1)+1;

kinect_config_x = 1:length(kinect_config_types);
kinect_config_avg_dx = zeros(rows,1);
kinect_config_std_dx = zeros(rows,1);
kinect_config_avg_dy = zeros(rows,1);
kinect_config_std_dy = zeros(rows,1);
kinect_config_avg_dz = zeros(rows,1);
kinect_config_std_dz = zeros(rows,1);
kinect_config_avg_dd = zeros(rows,1);
kinect_config_std_dd = zeros(rows,1);

row_counter = 1;
for kinect_config = unique(scen_table.Kinect_Config,'rows').'
    k_table = scen_table(scen_table.Kinect_Config==kinect_config,:);
    
    kinect_config_avg_dx(row_counter,1) = k_table{1,coordinate_dx_idx};
    kinect_config_std_dx(row_counter,1) = k_table{1,coordinate_dx_idx+1};
    kinect_config_avg_dy(row_counter,1) = k_table{1,coordinate_dy_idx};
    kinect_config_std_dy(row_counter,1) = k_table{1,coordinate_dy_idx+1};
    kinect_config_avg_dz(row_counter,1) = k_table{1,coordinate_dz_idx};
    kinect_config_std_dz(row_counter,1) = k_table{1,coordinate_dz_idx+1};
    kinect_config_avg_dd(row_counter,1) = k_table{1,coordinate_dd_idx};
    kinect_config_std_dd(row_counter,1) = k_table{1,coordinate_dd_idx+1};
    
    row_counter = row_counter+1;
end

avg_dx_idx = 3;
avg_dy_idx = 5;
avg_dz_idx = 7;
avg_dd_idx = 9;
average_table = coordinates_average_scenario_table(1,:);
kinect_config_avg_dx(row_counter,1) = average_table{1,avg_dx_idx};
kinect_config_std_dx(row_counter,1) = average_table{1,avg_dx_idx+1};
kinect_config_avg_dy(row_counter,1) = average_table{1,avg_dy_idx};
kinect_config_std_dy(row_counter,1) = average_table{1,avg_dy_idx+1};
kinect_config_avg_dz(row_counter,1) = average_table{1,avg_dz_idx};
kinect_config_std_dz(row_counter,1) = average_table{1,avg_dz_idx+1};
kinect_config_avg_dd(row_counter,1) = average_table{1,avg_dd_idx};
kinect_config_std_dd(row_counter,1) = average_table{1,avg_dd_idx+1};

figure;
hold on;
errorbar(kinect_config_x,kinect_config_avg_dx,kinect_config_std_dx,'MarkerEdgeColor',red,'MarkerFaceColor',red,'Color',red,'LineStyle','none','Marker','o');
errorbar(kinect_config_x,kinect_config_avg_dy,kinect_config_std_dy,'MarkerEdgeColor',green,'MarkerFaceColor',green,'Color',green,'LineStyle','none','Marker','o');
errorbar(kinect_config_x,kinect_config_avg_dz,kinect_config_std_dz,'MarkerEdgeColor',blue,'MarkerFaceColor',blue,'Color',blue,'LineStyle','none','Marker','o');
errorbar(kinect_config_x,kinect_config_avg_dd,kinect_config_std_dd,'MarkerEdgeColor',black,'MarkerFaceColor',black,'Color',black,'LineStyle','none','Marker','x','MarkerSize',10);
box on;
hold off;

title(main_title,'Fontsize',15);
xlabel({'','Kinect Configurations',''},'Fontsize',15);
ylabel('Distance (cm)','Fontsize',15);
set(gca,'XLim',[0.5 length(kinect_config_types)+0.5]);
set(gca,'XTick',1:length(kinect_config_types),'XTickLabel',kinect_config_types);
ax = gca;
ax.XTickLabelRotation = -90;
legend('\Delta x','\Delta y','\Delta z','\Delta d','Location','northwest');

set(gcf,'Visible','Off');
set(gcf,'PaperPositionMode','Manual');
set(gcf,'PaperUnits','Normalized');
print('-dsvg','-painters',main_filename);


end
