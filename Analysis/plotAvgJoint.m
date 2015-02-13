function [] = plotAvgJoint(avgerage_joint_table)
%PLOTALLJOINT Summary of this function goes here
%   Detailed explanation goes here
consts;

person_id = avgerage_joint_table(1,average_c_person);
plot_title = sprintf('Person %d Average Joint',person_id);
plot_filename = sprintf('Plots/Person_%d_AverageJoint',person_id);

x = avgerage_joint_table(:,average_c_timestamp);
dx_avg = avgerage_joint_table(:,average_c_dx_avg);
dx_sd = avgerage_joint_table(:,average_c_dx_sd);
dy_avg = avgerage_joint_table(:,average_c_dy_avg);
dy_sd = avgerage_joint_table(:,average_c_dy_sd);
dz_avg = avgerage_joint_table(:,average_c_dz_avg);
dz_sd = avgerage_joint_table(:,average_c_dz_sd);
dd_avg = avgerage_joint_table(:,average_c_dd_avg);
dd_sd = avgerage_joint_table(:,average_c_dd_sd);

figure;
hold on;
shadedErrorBar(x,dx_avg,dx_sd,'-r', 1);
shadedErrorBar(x,dy_avg,dy_sd,'-g', 1);
shadedErrorBar(x,dz_avg,dz_sd,'-b', 1);
shadedErrorBar(x,dd_avg,dd_sd,'-k', 1);
ylim([0 0.25]);
xlim([0 30000]);
% set(gcf,'visible','off')
hold off;

title(plot_title);
xlabel('Time(ms)');
ylabel('Distance(m)');
legend('Avg. \Delta x','Avg. \Delta y','Avg. \Delta z','Avg. \Delta d','Location','northeastoutside');
print('-dpdf', '-painters', plot_filename);

end

